<template>
  <div class="income-allocation-templates-view">
    <ListView
      header="IncomeAllocation"
      v-model="incomeAllocationTemplates"
      :update="updateIncomeAllocationTemplate"
      :remove="deleteIncomeAllocationTemplate"
    >
      <template #content="{ data }">
        <div class="income-allocation-template-view_body">
          <span>{{ data.name }}</span>
        </div>
      </template>
      <template #editor="{ data }">
        <IncomeAllocationInput
          :incomeAllocation="data"
          @changed="onIncomeAllocationChanged(data, $event)"
        />
      </template>
    </ListView>
  </div>
</template>
<script setup lang="ts">
import ListView from '@/components/ListView.vue';
import IncomeAllocationInput from './IncomeAllocationInput.vue';
import { IncomeAllocation } from '@/models/income-allocation';
import { useAppStore } from '@/store/store';
import { storeToRefs } from 'pinia';

const store = useAppStore();
const { updateIncomeAllocationTemplate, deleteIncomeAllocationTemplate } = store;

const { incomeAllocationTemplates } = storeToRefs(store);

function onIncomeAllocationChanged(incomeAllocation: IncomeAllocation, newValue: IncomeAllocation) {
  incomeAllocation.name = newValue.name;
  incomeAllocation.rules = newValue.rules;
  incomeAllocation.defaultFundId = newValue.defaultFundId;
  incomeAllocation.defaultFundName = newValue.defaultFundName;
}
</script>

<style lang="scss">
.income-allocation-templates-view {
  width: 100%;
  height: 100%;
  &_body {
    display: flex;
    width: 100%;
    span {
      text-align: left;
      padding-left: 1rem;
      display: inline-block;
      text-overflow: ellipsis;
      overflow: hidden;
    }
  }
  &_editor {
    display: flex;
  }
}
</style>
